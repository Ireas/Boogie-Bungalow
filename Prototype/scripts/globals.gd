extends Node

signal trigger_communication(command_id:int)
signal music_play_track(track_name:String)

signal stopptanz_initialized()
signal stopptanz_solved()

signal music_continue()
signal music_pause()


func update_global_ping(
	ping_4drinks:int, 
	ping_stopptanz:int, 
	ping_sparkasten:int, 
	ping_telefon:int,
	ping_wasserhahn:int,
	ping_sexdungeon:int, 
	ping_schichtplan:int, 
	ping_separee:int):
	update_ping.emit(
		ping_4drinks,
		ping_stopptanz,
		ping_sparkasten,
		ping_telefon,
		ping_wasserhahn,
		ping_sexdungeon,
		ping_schichtplan,
		ping_separee,
	)

signal update_ping(
	ping_4drinks:int, 
	ping_stopptanz:int, 
	ping_sparkasten:int, 
	ping_telefon:int,
	ping_wasserhahn:int,
	ping_sexdungeon:int, 
	ping_schichtplan:int, 
	ping_separee:int
)	
