extends Node

func request_finale():
	GameManager.game_session_finished.emit()
	await get_tree().create_timer(30).timeout
	GameManager.connector.call("SepareeWhite")
