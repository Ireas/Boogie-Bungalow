extends Button

@export var game_delayer : GameDelayer
@export var game_stopwatch : Stopwatch
@export var overlay : Panel
@export var music_player : MusicPlayer

func start_game():
	disabled = true
	start_countdown(game_delayer.current_delay)
	game_delayer.queue_free()
	
func start_countdown(delay:int):	
	var current_delay : int = delay
	while(current_delay>0):
		text = "game starts in %s seconds"%[current_delay]
		await get_tree().create_timer(1).timeout
		current_delay-= 1
	
	music_player.play_track("atmo1")
	game_stopwatch.start()
	overlay.queue_free()
	queue_free()
