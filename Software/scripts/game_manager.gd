extends Node

# CONSTANTS
### ignore no coms problem 
var WORKING_FROM_HOME = false

### preload scenes
@export var LOADING_SCREEN : PackedScene = preload("res://scenes/loading_screen.tscn")
@export var GAME_SESSION : PackedScene = preload("res://scenes/game_session.tscn")


# VARIABLES
var game_start_button : Control
var pregame_timer : Control
var connector : Node # connector node registers itself
var loading_screen : LoadingScreen
var session : GameSession 

# SIGNALS
signal new_game_session_started
signal pregame_timer_started
signal pregame_timer_canceled
signal game_session_started
signal game_session_finished

# SETUP
func _ready():
	loading_screen = LOADING_SCREEN.instantiate()
	loading_screen.loading_screen_clicked.connect(request_master_ack)
	get_node("/root/Universe").add_child(loading_screen)

func connect_to_master():
	loading_screen.set_percentage(10)
	loading_screen.set_message("Öffne Master Port...")
	var verification : bool = connector.call("OpenMasterPort")
	
	if verification:
		request_master_ack()
	else:
		loading_screen.set_message("Verbindung unmöglich: Port nicht richtig konfiguriert!")
		
	if WORKING_FROM_HOME: # ignore everything and emulate feeling
		await get_tree().create_timer(0.5).timeout
		request_master_ack()


func request_master_ack():
	loading_screen.set_percentage(30)
	loading_screen.set_message("Warte auf Master ACK...")
	var verification : bool = connector.call("WaitForMasterACK")
	
	if verification:
		loading_screen.delete()
		start_new_game_session()
	else:
		loading_screen.set_message("Verbindung unmöglich: Ist der Rätselstrom angeschalten?")

	if WORKING_FROM_HOME: # ignore everything and emulate feeling
		await get_tree().create_timer(2).timeout
		loading_screen.delete()
		start_new_game_session()



func start_new_game_session():
	if session:
		var previous_delay = session.previous_delay
		session = GAME_SESSION.instantiate()
		session.pregame_delay = previous_delay
	else:
		session = GAME_SESSION.instantiate()
	
	new_game_session_started.emit()

func restart_session():
	connector.call("SystemHardReset")
	start_new_game_session()
