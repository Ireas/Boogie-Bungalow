extends Button

func _on_button_down():
	Globals.music_play_track.emit("atmo1")

