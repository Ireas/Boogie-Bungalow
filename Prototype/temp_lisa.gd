extends Button

enum Version{
	FULL,
	SHORT,
}

@export var player : MusicPlayer
@export var rather_be_full : AudioStream
@export var rather_be_short : AudioStream
@export var version : Version


func start_song():
	if version==Version.FULL:
		player.stream = rather_be_full
	else:
		player.stream = rather_be_short
	
	player.playing = true
