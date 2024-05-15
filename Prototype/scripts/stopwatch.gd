extends Node
class_name Stopwatch

@export var label : Label
@export var timer : Timer
var hours : int
var minutes : int
var seconds : int

func _ready():
	timer.timeout.connect(_add_second)

func start():
	timer.start()

func _add_second():
	seconds+= 1
	
	if(seconds==60):
		minutes+= 1
		seconds-= 60
	if(minutes==60):
		hours+= 1
		minutes-= 60
	
	label.text = "%02d:%02d:%02d"%[hours,minutes,seconds]
