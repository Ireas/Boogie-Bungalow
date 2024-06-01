extends Control
class_name Stopwatch

# EXPORT
@export var duration_label : Label

# VARIABLES
var stop : bool = false


func _ready():
	GameManager.new_game_session_started.connect(setup)
	GameManager.game_session_started.connect(start)

func setup():
	GameManager.session.game_duration_changed.connect(update_ui)
	GameManager.game_session_finished.connect( func(): stop = true )
	update_ui()

func update_ui():
	var hours : int = GameManager.session.game_duration/3600
	var minutes : int = GameManager.session.game_duration/60 - hours * 60
	var seconds : int = GameManager.session.game_duration%60
	duration_label.text = "%02d:%02d:%02d"%[hours,minutes,seconds]

func start():
	stop = false
	while not stop:
		await get_tree().create_timer(1).timeout
		GameManager.session.game_duration+= 1
