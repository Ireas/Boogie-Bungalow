extends Button

func _ready():
	GameManager.game_session_finished.connect( func(): visible = true )
	button_down.connect(ask_for_restart)

func ask_for_restart():
	var popup : CustomPopup = PopupManager.generate_popup("Durch eine Spielsession werden alle Rätsel und Verbindungen zurückgesetzt. \nDauer ca. 30s (Software muss nicht geschlossen werden, Rätselstrom nicht aus- und angeschatlten werden.)")
	popup.response_confirm.connect(GameManager.restart_session)
	popup.response_confirm.connect( func() : visible = false )
