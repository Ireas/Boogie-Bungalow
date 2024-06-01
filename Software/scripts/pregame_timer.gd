extends Control
class_name PregameTimer

# EXPORT
@export var pregame_delay_label : Label
@export var buttons_change_time : Control
@export var buttons_time_control : Control

# VARIABLES
var cancel_countdown : bool = true

func _ready():
	GameManager.new_game_session_started.connect(setup)
	GameManager.game_session_finished.connect(cancel)

func setup():
	cancel() #prevent game countdown while new game started
	visible = true
	buttons_change_time.visible = true
	buttons_time_control.visible = false
	GameManager.session.pregame_delay_changed.connect(update_ui)
	update_ui()

func update_ui():
	var minutes = int(GameManager.session.pregame_delay/60)
	var seconds = int(GameManager.session.pregame_delay%60)
	pregame_delay_label.text = "%02d:%02d"%[minutes,seconds]


func increase_delay(delay_increase:int):
	GameManager.session.pregame_delay+= delay_increase

func skip():
	GameManager.session.pregame_delay = 0

func cancel():
	cancel_countdown = true


func start():
	GameManager.pregame_timer_started.emit()
	GameManager.session.previous_delay = GameManager.session.pregame_delay
	cancel_countdown = false
	buttons_change_time.visible = false
	buttons_time_control.visible = true
	
	while(GameManager.session.pregame_delay>0):
		if cancel_countdown:
			GameManager.session.pregame_delay = GameManager.session.previous_delay
			buttons_change_time.visible = true
			buttons_time_control.visible = false
			GameManager.pregame_timer_canceled.emit()
			return
		GameManager.session.pregame_delay-= 1
		await get_tree().create_timer(1).timeout
	
	visible = false
	GameManager.session.start()
