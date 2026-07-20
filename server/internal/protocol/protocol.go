// Package protocol defines the client/server compatibility contract.
// Authoritative file: shared/protocol.json — sync with: python scripts/sync_shared_embeds.py
package protocol

import (
	_ "embed"
	"encoding/json"
	"fmt"
	"net/http"
)

//go:generate py -3 ../../../../scripts/sync_shared_embeds.py
//go:embed protocol.json
var specBytes []byte

// Spec is loaded from shared/protocol.json.
type Spec struct {
	Protocol          int    `json:"protocol"`
	MinClientProtocol int    `json:"minClientProtocol"`
	MaxClientProtocol int    `json:"maxClientProtocol"`
	Description       string `json:"description"`
}

// Info is returned by GET /version.
type Info struct {
	Release   string `json:"release"`
	Protocol  int    `json:"protocol"`
	MinClient int    `json:"minClient"`
	MaxClient int    `json:"maxClient"`
}

// Mismatch is returned when client and server protocols are incompatible.
type Mismatch struct {
	Error     string `json:"error"`
	Reason    string `json:"reason"`
	Message   string `json:"message"`
	Client    int    `json:"clientProtocol"`
	Server    int    `json:"serverProtocol"`
	MinClient int    `json:"minClient"`
	MaxClient int    `json:"maxClient"`
}

const (
	ReasonOutdatedClient = "outdated_client"
	ReasonOutdatedServer = "outdated_server"

	// HeaderName is sent by the client on HTTP auth and WebSocket upgrade.
	HeaderName = "X-Deathborn-Protocol"
)

var current Spec

func init() {
	if err := json.Unmarshal(specBytes, &current); err != nil {
		panic("protocol: invalid shared/protocol.json: " + err.Error())
	}
}

func Current() Spec { return current }

func InfoForRelease(release string) Info {
	s := current
	return Info{
		Release:   release,
		Protocol:  s.Protocol,
		MinClient: s.MinClientProtocol,
		MaxClient: s.MaxClientProtocol,
	}
}

// CheckClient returns whether a client protocol can connect to this server.
func CheckClient(clientProtocol int) (ok bool, mismatch Mismatch) {
	s := current
	mismatch = Mismatch{
		Client:    clientProtocol,
		Server:    s.Protocol,
		MinClient: s.MinClientProtocol,
		MaxClient: s.MaxClientProtocol,
	}

	if clientProtocol <= 0 {
		mismatch.Reason = ReasonOutdatedClient
		mismatch.Message = fmt.Sprintf(
			"Outdated client! Missing protocol version (server protocol %d, accepts clients %d–%d).",
			s.Protocol, s.MinClientProtocol, s.MaxClientProtocol,
		)
		mismatch.Error = mismatch.Message
		return false, mismatch
	}

	if clientProtocol < s.MinClientProtocol {
		mismatch.Reason = ReasonOutdatedClient
		mismatch.Message = fmt.Sprintf(
			"Outdated client! Please update Deathborn (your protocol %d; server %d accepts %d–%d).",
			clientProtocol, s.Protocol, s.MinClientProtocol, s.MaxClientProtocol,
		)
		mismatch.Error = mismatch.Message
		return false, mismatch
	}

	if clientProtocol > s.MaxClientProtocol {
		mismatch.Reason = ReasonOutdatedServer
		mismatch.Message = fmt.Sprintf(
			"Outdated server! Cannot join with this client (client protocol %d; server %d accepts %d–%d).",
			clientProtocol, s.Protocol, s.MinClientProtocol, s.MaxClientProtocol,
		)
		mismatch.Error = mismatch.Message
		return false, mismatch
	}

	return true, mismatch
}

func WriteMismatch(w http.ResponseWriter, mismatch Mismatch) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusConflict)
	_ = json.NewEncoder(w).Encode(mismatch)
}

func WriteInfo(w http.ResponseWriter, info Info) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	_ = json.NewEncoder(w).Encode(info)
}
