class_name DevCredentials
extends RefCounted
## Persists the last successful login locally for one-click dev re-entry.
## Only read/written when OS.is_debug_build() is true (editor F5, debug exports).
## Release/production builds never touch this file and must not expose the UI.

const PATH := "user://dev_last_login.cfg"


static func is_available() -> bool:
	return OS.is_debug_build()


static func save(email: String, password: String) -> void:
	if not is_available():
		return
	var cfg := ConfigFile.new()
	cfg.set_value("auth", "email", email.strip_edges().to_lower())
	cfg.set_value("auth", "password", password)
	cfg.save(PATH)


static func load_saved() -> Dictionary:
	if not is_available():
		return {}
	var cfg := ConfigFile.new()
	if cfg.load(PATH) != OK:
		return {}
	var email: String = String(cfg.get_value("auth", "email", ""))
	var password: String = String(cfg.get_value("auth", "password", ""))
	if email.is_empty() or password.is_empty():
		return {}
	return {"email": email, "password": password}


static func has_saved() -> bool:
	return not load_saved().is_empty()
