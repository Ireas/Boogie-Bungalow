extends Node

# game session management
signal session_start()
signal session_finish()

# block ui signal
signal button_press()


# sync updates
signal sync_package_send()
signal sync_successful()

# riddle status update
signal update_status_drinks(ping:int, solved:int, state:int)
signal update_status_stopptanz(ping:int, solved:int, state:int)
signal update_status_sparkasten(ping:int, solved:int, state:int)
signal update_status_telefon(ping:int, solved:int, state:int)
signal update_status_sexdungeon(ping:int, solved:int, state:int)
signal update_status_schichtplan(ping:int, solved:int, state:int)
signal update_status_separee(ping:int, solved:int, state:int)

func update_riddles(
	drinks_ping:int, drinks_solved:int, drinks_state:int,
	stopptanz_ping:int, stopptanz_solved:int, stopptanz_state:int,
	sparkasten_ping:int, sparkasten_solved:int, sparkasten_state:int,
	telefon_ping:int, telefon_solved:int, telefon_state:int,
	sexdungeon_ping:int, sexdungeon_solved:int, sexdungeon_state:int,
	schichtplan_ping:int, schichtplan_solved:int, schichtplan_state:int,
	separee_ping:int, separee_solved:int, separee_state:int
):
	update_status_drinks.emit(drinks_ping, drinks_solved, drinks_state)
	update_status_stopptanz.emit(stopptanz_ping, stopptanz_solved, stopptanz_state)
	update_status_sparkasten.emit(sparkasten_ping, sparkasten_solved, sparkasten_state)
	update_status_telefon.emit(telefon_ping, telefon_solved, telefon_state)
	update_status_sexdungeon.emit(sexdungeon_ping, sexdungeon_solved, sexdungeon_state)
	update_status_schichtplan.emit(schichtplan_ping, schichtplan_solved, schichtplan_state)
	update_status_separee.emit(separee_ping, separee_solved, separee_state)
