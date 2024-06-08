extends VBoxContainer

signal on_soft_reset()
signal on_hard_reset()

func _ready():
	visible = false
	GameManager.game_session_started.connect( func(): visible=true )
	GameManager.game_session_finished.connect( func(): visible=false )


func soft_reset():
	var popup : CustomPopup = PopupManager.generate_popup("Durch einen Soft Reset wird das Netzwerk zurückgesetzt.\nSchwache Verbindungen können so wiederhergestellt werden [color=violet]ohne die Rätsel zu resetten[/color].\nDauer ca. 15s (Währenddessen ist keine Bedienung möglich!)")
	popup.response_confirm.connect( func(): on_soft_reset.emit() )

func hard_reset():
	var popup : CustomPopup = PopupManager.generate_popup("Durch einen Hard Reset wird das komplette Netzwerk neu synchronisiert.\nDer [color=violet]Status der Rätsel wird dabei zurückgesetzt[/color]. Probiert also vorher den Soft Reset!\nDauer ca. 30s ([color=violet]Warte bis Rätsel wieder grüne Verbindung[/color] haben.)")
	popup.response_confirm.connect( func(): on_hard_reset.emit() )
