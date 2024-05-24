extends Panel
class_name CustomPopup

@export var label : Label

signal response_confirm()
signal response_cancel()

func setup(message:String):
	label.text = message


func confirm():
	response_confirm.emit()
	queue_free()

func cancel():
	response_cancel.emit()
	queue_free()
