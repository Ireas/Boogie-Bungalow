extends Node


const WHITE_LIGHT_AFTER_FINISH_DOWNTIME : float = 30

@export var music_player : MusicPlayer

signal light_white

func finish_game():
	EventBus.session_finish.emit()
	await get_tree().create_timer(WHITE_LIGHT_AFTER_FINISH_DOWNTIME).timeout
	light_white.emit()
