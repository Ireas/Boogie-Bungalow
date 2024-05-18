extends Node


@export var music_player : MusicPlayer

func music_switch_to_stoptanz():
	music_player.play_track("stopptanz")

func music_switch_to_atmo2():
	music_player.play_track("atmo2")

var separee_is_playing = false

func music_switch_to_separee():
	if separee_is_playing:
		return
	separee_is_playing = true
	music_player.play_track("separee")
	
func music_switch_to_finale():
	music_player.play_track("finale")

signal light_white

func delayed_white_light():
	await get_tree().create_timer(30).timeout
	light_white.emit()
