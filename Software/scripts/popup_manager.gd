extends Node

var popup_scene : PackedScene = preload("res://modules/popup/popup.tscn")

func generate_popup(message:String) -> CustomPopup:
	var popup : CustomPopup = popup_scene.instantiate()
	get_node("/root/Universe").add_child(popup)
	popup.setup(message)
	return popup
