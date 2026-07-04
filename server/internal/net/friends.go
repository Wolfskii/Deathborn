package net

import (
	"context"
	"strings"

	"github.com/deathborn/server/internal/db"
)

type FriendEntry struct {
	AccountID   int64  `json:"accountId"`
	Name        string `json:"name"`
	CharacterID int64  `json:"characterId,omitempty"`
	Online      bool   `json:"online"`
	PendingIn   bool   `json:"pendingIn,omitempty"`
	PendingOut  bool   `json:"pendingOut,omitempty"`
}

type FriendsData struct {
	Friends []FriendEntry `json:"friends"`
}

type FriendAddSendData struct {
	TargetCharacterID int64 `json:"targetCharacterId"`
}

type FriendRespondSendData struct {
	FromAccountID int64 `json:"fromAccountId"`
	Accept        bool  `json:"accept"`
}

type FriendRemoveSendData struct {
	FriendAccountID int64 `json:"friendAccountId"`
}

type PmSendData struct {
	TargetCharacterID int64  `json:"targetCharacterId"`
	Text              string `json:"text"`
}

type PmData struct {
	FromCharacterID int64  `json:"fromCharacterId"`
	FromName        string `json:"fromName"`
	FromAccountID   int64  `json:"fromAccountId,omitempty"`
	Text            string `json:"text"`
	Outgoing        bool   `json:"outgoing,omitempty"`
}

func BuildFriends(d FriendsData) []byte {
	return encode("friends", d)
}

func BuildPm(d PmData) []byte {
	return encode("pm", d)
}

func (h *Hub) syncFriendsToAccount(accountID int64) {
	ctx := context.Background()
	database := h.db
	if database == nil || accountID <= 0 {
		return
	}

	friendIDs, err := database.ListFriendAccountIDs(ctx, accountID)
	if err != nil {
		return
	}
	incoming, _ := database.ListIncomingRequests(ctx, accountID)
	outgoing, _ := database.ListOutgoingRequests(ctx, accountID)

	seen := map[int64]bool{}
	var entries []FriendEntry

	addEntry := func(peerAccountID int64, pendingIn, pendingOut bool) {
		if seen[peerAccountID] {
			return
		}
		seen[peerAccountID] = true
		name, _ := database.DisplayNameForAccount(ctx, peerAccountID)
		charID, online := h.OnlineCharacter(peerAccountID)
		entries = append(entries, FriendEntry{
			AccountID:   peerAccountID,
			Name:        name,
			CharacterID: charID,
			Online:      online,
			PendingIn:   pendingIn,
			PendingOut:  pendingOut,
		})
	}

	for _, id := range friendIDs {
		addEntry(id, false, false)
	}
	for _, id := range incoming {
		addEntry(id, true, false)
	}
	for _, id := range outgoing {
		addEntry(id, false, true)
	}

	if entries == nil {
		entries = []FriendEntry{}
	}
	h.SendToAccount(accountID, BuildFriends(FriendsData{Friends: entries}))
}

func (h *Hub) SendToAccount(accountID int64, msg []byte) {
	if accountID <= 0 {
		return
	}
	charID, ok := h.OnlineCharacter(accountID)
	if !ok || charID <= 0 {
		return
	}
	h.SendToCharacter(charID, msg, false)
}

func (h *Hub) syncFriendsForBoth(accountA, accountB int64) {
	h.syncFriendsToAccount(accountA)
	h.syncFriendsToAccount(accountB)
}

func (c *Client) handleFriendAdd(database *db.DB, targetCharacterID int64) {
	if targetCharacterID <= 0 || targetCharacterID == c.characterID {
		return
	}
	ctx := context.Background()
	targetAccount, err := database.GetCharacterAccountID(ctx, targetCharacterID)
	if err != nil {
		c.safeSend(encode("error", MessageData{Message: "That player was not found."}))
		return
	}
	if targetAccount == c.accountID {
		c.safeSend(encode("error", MessageData{Message: "You cannot add yourself."}))
		return
	}
	if ok, _ := database.AreFriends(ctx, c.accountID, targetAccount); ok {
		c.safeSend(encode("error", MessageData{Message: "Already friends."}))
		return
	}
	if err := database.SendFriendRequest(ctx, c.accountID, targetAccount); err != nil {
		if err == db.ErrRequestExists {
			c.safeSend(encode("error", MessageData{Message: "Friend request already sent."}))
		}
		return
	}
	c.hub.syncFriendsForBoth(c.accountID, targetAccount)
}

func (c *Client) handleFriendRespond(database *db.DB, fromAccountID int64, accept bool) {
	if fromAccountID <= 0 || fromAccountID == c.accountID {
		return
	}
	ctx := context.Background()
	if accept {
		if err := database.AcceptFriendRequest(ctx, c.accountID, fromAccountID); err != nil {
			if err == db.ErrNoRequest {
				c.safeSend(encode("error", MessageData{Message: "No pending friend request."}))
			}
			return
		}
	} else {
		_, _ = database.Pool.Exec(ctx,
			`DELETE FROM friend_requests WHERE from_account_id = $1 AND to_account_id = $2`,
			fromAccountID, c.accountID,
		)
	}
	c.hub.syncFriendsForBoth(c.accountID, fromAccountID)
}

func (c *Client) handleFriendRemove(database *db.DB, friendAccountID int64) {
	if friendAccountID <= 0 || friendAccountID == c.accountID {
		return
	}
	ctx := context.Background()
	_ = database.RemoveFriend(ctx, c.accountID, friendAccountID)
	c.hub.syncFriendsForBoth(c.accountID, friendAccountID)
}

func (c *Client) handlePmSend(database *db.DB, targetCharacterID int64, text string) {
	if !c.spawned || targetCharacterID <= 0 || targetCharacterID == c.characterID {
		return
	}
	text = strings.TrimSpace(text)
	if text == "" || len(text) > 120 {
		return
	}
	ctx := context.Background()
	fromName, err := database.DisplayNameForAccount(ctx, c.accountID)
	if err != nil {
		fromName = "Unknown"
	}
	outgoing := BuildPm(PmData{
		FromCharacterID: c.characterID,
		FromName:        fromName,
		FromAccountID:   c.accountID,
		Text:            text,
		Outgoing:        true,
	})
	incoming := BuildPm(PmData{
		FromCharacterID: c.characterID,
		FromName:        fromName,
		FromAccountID:   c.accountID,
		Text:            text,
	})
	c.safeSend(outgoing)
	c.hub.SendToCharacter(targetCharacterID, incoming, false)
}
