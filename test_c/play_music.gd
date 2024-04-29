extends Button

@export var music_player : AudioStreamPlayer
@export var music_tracks: Array[AudioStream]

func _ready():
	print("Songs found: " + str(len(music_tracks)))

func _on_button_down():
	if not music_player.playing:
		music_player.stream = music_tracks[0]
		music_player.playing = true
	else:
		music_player.playing = false
		
