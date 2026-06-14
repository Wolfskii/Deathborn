// Package migrations embeds the SQL schema files so they can be applied at
// server startup without shipping the .sql files alongside the binary.
package migrations

import "embed"

//go:embed *.sql
var Files embed.FS
