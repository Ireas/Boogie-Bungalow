extends Panel
class_name CustomPopup

@export var label : RichTextLabel

signal response_confirm()
signal response_cancel()

func setup(message:String):
	label.text = "[center]" + message


func confirm():
	response_confirm.emit()
	queue_free()

func cancel():
	response_cancel.emit()
	queue_free()
