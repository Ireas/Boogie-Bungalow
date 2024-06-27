extends AudioStreamPlayer
class_name MusicPlayer

@export var animator : AnimationPlayer

var DIRECTORY_MUSIC_TRACKS : String = "user://Musik/"
var DICT_MUSIC_TRACKS : Dictionary = {
	"atmo1" : "atmo1.mp3",
	"atmo2" : "atmo2.mp3",
	"stopptanz" : "stopptanz.mp3",
	"separee" : "separee.mp3",
	"finale" : "finale.mp3",
}

var dict_loaded_music_tracks : Dictionary = {}
var stopptanz_ongoing : bool = false

func _ready():
	if not DirAccess.dir_exists_absolute(DIRECTORY_MUSIC_TRACKS):
		print(">>>[E]: folder \"%s\" not found"%[DIRECTORY_MUSIC_TRACKS])
	
	for track_name in DICT_MUSIC_TRACKS:
		var track_file_name : String = DICT_MUSIC_TRACKS[track_name]
		if not FileAccess.file_exists(DIRECTORY_MUSIC_TRACKS+track_file_name):
			print(" >>[W]: music track \"%s\" not found"%[track_file_name])
			continue
		
		var music_file = FileAccess.open(DIRECTORY_MUSIC_TRACKS+track_file_name, FileAccess.READ)	
		var mp3_file = AudioStreamMP3.new()
		mp3_file.data = music_file.get_buffer(music_file.get_length())
		dict_loaded_music_tracks[track_name] = mp3_file
		
	GameManager.game_session_started.connect( func(): play_track("atmo1", true) )
	GameManager.new_game_session_started.connect( func(): pause() )
	

func mark_stopptanz(status:bool):
	# disable connecting/disconnecting multiple times
	if stopptanz_ongoing==status:
		return
	
	stopptanz_ongoing = status
	
	if status==true:
		EventBus.update_status_stopptanz.connect(stopptanz_react)
		print("  >[V]: stoptanz now updating pause and play")
	else:
		EventBus.update_status_stopptanz.disconnect(stopptanz_react)
		print("  >[V]: stoptanz no longer updating pause and play")


func stopptanz_react(_ping:int, _solved:int, state:int):
	if not stopptanz_ongoing:
		return
	
	if state==2:
		pause()
	else:
		unpause()


func play_track(track_name:String, force:bool=false):
	if not track_name in dict_loaded_music_tracks:
		print(" >>[W]: music track \"%s\" not found"%[track_name])
		return
	
	# if stream is already playing, then skip command
	if not force and stream==dict_loaded_music_tracks[track_name]:
		return

	animator.play("music_fade_out", -1, 0.5)
	await animator.animation_finished
	stream = dict_loaded_music_tracks[track_name]
	animator.play("music_fade_in", -1, 0.35)
	playing = true


func pause():
	stream_paused = true

func unpause():
	stream_paused = false


func _on_finished():
	play_track("atmo1")
