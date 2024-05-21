extends Node

signal start_sync()

@export var animator : AnimationPlayer
@export var progress_bar : ProgressBar
@export var skip_sync : bool = false

var disabled : bool = false
var block_finish : bool = false


func _ready():
	if skip_sync:
		queue_free()
		return
	
	EventBus.sync_package_send.connect( func(): progress_bar.value+= 1 )
	EventBus.sync_successful.connect( finish_connecting )

func _input(event):
	if disabled:
		return
	
	if event is InputEventMouseButton and event.button_index==1:
		disabled = true
		start_connecting()

func start_connecting():
	animator.play("fade_in")
	start_sync.emit()
	
func finish_connecting():
	if block_finish:
		return
		
	block_finish = true
	progress_bar.value+= 2
	animator.play("fade_out")
	await animator.animation_finished
	queue_free()
	
