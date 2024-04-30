extends Button

@export var music_player : AudioStreamPlayer

var DIRECTORY_MUSIC_TRACKS : String = "user://Musik/"
var DICT_MUSIC_TRACKS : Dictionary = {
	"Atmo1" : "atmo1.mp3",
	"Atmo2" : "atmo2.mp3",
	"Stopptanz" : "stopptanz.mp3",
	"Separee" : "separee.mp3",
	"Finale" : "finale.mp3",
}
var dict_loaded_music_tracks : Dictionary = {}

func _ready():
	if not DirAccess.dir_exists_absolute(DIRECTORY_MUSIC_TRACKS):
		print("Error: folder \"%s\" not found"%[DIRECTORY_MUSIC_TRACKS])
	
	for track_name in DICT_MUSIC_TRACKS:
		var track_file_name : String = DICT_MUSIC_TRACKS[track_name]
		if not FileAccess.file_exists(DIRECTORY_MUSIC_TRACKS+track_file_name):
			print("Warning: music track \"%s\" not found"%[track_file_name])
			continue
		
		var music_file = FileAccess.open(DIRECTORY_MUSIC_TRACKS+track_file_name, FileAccess.READ)	
		var mp3_file = AudioStreamMP3.new()
		mp3_file.data = music_file.get_buffer(music_file.get_length())
		dict_loaded_music_tracks[track_name] = mp3_file
		print("Verbose: music track \"%s\" found"%[track_file_name])
		

func _on_button_down():
	print("Verbose: trying to play music track \"Atmo1\"")
	if not music_player.playing:
		if "Atmo1" in dict_loaded_music_tracks:
			music_player.stream = dict_loaded_music_tracks["Atmo1"]
			music_player.playing = true
	else:
		music_player.playing = false
		
