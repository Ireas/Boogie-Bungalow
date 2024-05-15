extends Node

@export var music_player : MusicPlayer
@export var music_delay : float = 5

func start_game():
	await get_tree().create_timer(music_delay).timeout
	music_player.play_track("atmo1")

func music_switch_to_stoptanz():
	music_player.play_track("stopptanz")

func music_switch_to_atmo2():
	music_player.play_track("atmo2")

func music_switch_to_separee():
	music_player.play_track("separee")
	
func music_switch_to_finale():
	music_player.play_track("finale")
