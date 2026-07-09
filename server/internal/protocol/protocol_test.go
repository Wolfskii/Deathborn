package protocol

import "testing"

func TestCheckClient(t *testing.T) {
	s := Current()
	t.Run("accepts current", func(t *testing.T) {
		ok, _ := CheckClient(s.Protocol)
		if !ok {
			t.Fatal("expected compatible")
		}
	})
	t.Run("rejects too old", func(t *testing.T) {
		ok, m := CheckClient(s.MinClientProtocol - 1)
		if ok || m.Reason != ReasonOutdatedClient {
			t.Fatalf("got ok=%v reason=%q", ok, m.Reason)
		}
	})
	t.Run("rejects too new", func(t *testing.T) {
		ok, m := CheckClient(s.MaxClientProtocol + 1)
		if ok || m.Reason != ReasonOutdatedServer {
			t.Fatalf("got ok=%v reason=%q", ok, m.Reason)
		}
	})
}
