extends Button

@export var game_delayer : GameDelayer
@export var overlay : Panel
@export var music_player : MusicPlayer

@export var pause_button : Button
@export var skip_button : Button

var pause : bool = false
var remaining_delay : float = 0

func _ready():
	pause_button.button_down.connect(trigger_pause)
	skip_button.button_down.connect(start)

func start_game():
	disabled = true
	start_countdown(game_delayer.current_delay)
	game_delayer.queue_free()

func start_countdown(delay:int):
	pause_button.visible = true
	skip_button.visible = true
	remaining_delay = delay
	while(remaining_delay>0):
		text = "Spielstart in %s Sekunden"%[remaining_delay]
		await get_tree().create_timer(1).timeout
		if not pause:
			remaining_delay-= 1
	
	start()


func start():
	EventBus.session_start.emit()
	overlay.queue_free()
	pause_button.queue_free()
	skip_button.queue_free()
	queue_free()

func trigger_pause():
	pause = not pause
	if pause:
		pause_button.text = "Countdown Fortsetzen"
	else:
		pause_button.text = "Countdown Pausieren"
