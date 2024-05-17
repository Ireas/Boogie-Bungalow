extends AudioStreamPlayer
class_name MusicPlayer

@export var rather_be_file : AudioStream
@export var rather_be_file_alt : AudioStream
func play_ratherbe():
	stream = rather_be_file
	playing = true
func play_ratherbe_alt():
	stream = rather_be_file_alt
	playing = true

var DIRECTORY_MUSIC_TRACKS : String = "user://Musik/"
var DICT_MUSIC_TRACKS : Dictionary = {
	"atmo1" : "atmo1.mp3",
	"atmo2" : "atmo2.mp3",
	"stopptanz" : "stopptanz.mp3",
	"separee" : "separee.mp3",
	"finale" : "finale.mp3",
}

var dict_loaded_music_tracks : Dictionary = {}


func _ready():
	Globals.music_play_track.connect(play_track)
	Globals.music_pause.connect( func(): stream_paused = true )
	Globals.music_continue.connect( func(): stream_paused = false )

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
		
	
func play_track(track_name:String):
	if not track_name in dict_loaded_music_tracks:
		print("Warning: music track \"%s\" not found"%[track_name])
		return
	
	stream = dict_loaded_music_tracks[track_name]
	playing = true
