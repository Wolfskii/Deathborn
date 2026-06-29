// Package config loads server configuration from environment variables with
// sensible local-development defaults.
package config

import "os"

type Config struct {
	DatabaseURL string
	JWTSecret   string
	Port        string
	ListenHost  string
}

func Load() Config {
	return Config{
		DatabaseURL: getenv("DATABASE_URL", "postgres://deathborn:deathborn@localhost:5432/deathborn?sslmode=disable"),
		JWTSecret:   getenv("JWT_SECRET", "dev-secret-change-me"),
		Port:        getenv("PORT", "8080"),
		ListenHost:  getenv("LISTEN_HOST", ""),
	}
}

// HTTPAddr returns the address passed to http.Server. An empty ListenHost binds
// all interfaces (":port"), which is what Docker/production expects. Set
// LISTEN_HOST=127.0.0.1 for local dev to avoid Windows Firewall prompts.
func (c Config) HTTPAddr() string {
	if c.ListenHost != "" {
		return c.ListenHost + ":" + c.Port
	}
	return ":" + c.Port
}

func getenv(key, def string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return def
}
