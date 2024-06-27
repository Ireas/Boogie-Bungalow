extends Node

# CONSTANTS
var WORKING_FROM_HOME = false # ignore coms if working at home with no arduino

@export var LOADING_SCREEN : PackedScene = preload("res://scenes/loading_screen.tscn")
@export var GAME_SESSION : PackedScene = preload("res://scenes/game_session.tscn")


# VARIABLES
var connector : Node # connector node registers itself
var game_start_button : Control
var pregame_timer : Control
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
	print(OS.has_environment("USERNAME"))
	print(OS.get_environment("USERNAME"))
	# detect if im working from home to skip connecting to coms
	if OS.has_environment("USERNAME") and OS.get_environment("USERNAME")=="ireas":
		WORKING_FROM_HOME = true
	
	loading_screen = LOADING_SCREEN.instantiate()
	loading_screen.loading_screen_clicked.connect(connect_to_master)
	get_node("/root/Universe").add_child(loading_screen)

func connect_to_master():
	loading_screen.set_percentage(10)
	loading_screen.set_message("Öffne Master Port...")
	var verification : bool = connector.call("OpenMasterPort")
	
	if verification:
		request_master_ack()
	else:
		loading_screen.set_message("Fehler: Port Fehlkonfiguration => Bitte Ireas schreiben!")
	
	if WORKING_FROM_HOME:
		loading_screen.delete()
		start_new_game_session()


func request_master_ack():
	loading_screen.set_percentage(30)
	loading_screen.set_message("Warte auf Master ACK...")
	var verification : bool = connector.call("WaitForMasterACK")
	
	if verification:
		loading_screen.delete()
		start_new_game_session()
	else:
		loading_screen.set_message("Ist der Rätselstrom angeschalten? Klicken für erneuten Versuch")
		loading_screen.disabled = false



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
	connector.call("CreateNewLogger")
	loading_screen = LOADING_SCREEN.instantiate()
	loading_screen.loading_screen_clicked.connect(fake_loading)
	loading_screen.set_message("Bereite nächstes Spiel vor...")
	get_node("/root/Universe").add_child(loading_screen)
	start_new_game_session()

func fake_loading(): # fake loading to give better feeling at restart
	loading_screen.set_percentage(20)
	await get_tree().create_timer(3).timeout
	loading_screen.set_percentage(50)
	await get_tree().create_timer(1.5).timeout
	loading_screen.set_percentage(60)
	await get_tree().create_timer(10).timeout
	loading_screen.set_percentage(100)
	loading_screen.delete()
