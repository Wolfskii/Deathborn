package game

// NpcName returns the display name of a boss or mob by its world id.
// Bosses and mobs have negative ids, so this is also how a death's killer
// is named when an NPC landed the final blow.
func (w *World) NpcName(id int64) (string, bool) {
	w.mu.RLock()
	defer w.mu.RUnlock()
	if w.bossMgr != nil {
		if b, ok := w.bossMgr.bosses[id]; ok {
			return b.name, true
		}
	}
	if w.mobMgr != nil {
		if m, ok := w.mobMgr.mobs[id]; ok {
			return m.name, true
		}
	}
	return "", false
}

// SetLastChat remembers a player's most recent chat line for their epitaph.
func (w *World) SetLastChat(id int64, text string) {
	w.mu.Lock()
	defer w.mu.Unlock()
	if p, ok := w.players[id]; ok {
		p.lastChat = text
	}
}

// LastChat returns a player's most recent chat line, or "" if none.
func (w *World) LastChat(id int64) string {
	w.mu.RLock()
	defer w.mu.RUnlock()
	if p, ok := w.players[id]; ok {
		return p.lastChat
	}
	return ""
}
