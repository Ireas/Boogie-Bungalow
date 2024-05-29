extends Node
class_name GameSession


# VARIABLES
var pregame_delay : int = 150 : 
	set(value):
		pregame_delay = value if value>0 else 0
		pregame_delay_changed.emit()

var game_duration : int = 0 : 
	set(value):
		game_duration = value if value>0 else 0
		game_duration_changed.emit()

# SIGNALS
signal pregame_delay_changed
signal game_duration_changed


func start():
	GameManager.game_session_started.emit()


