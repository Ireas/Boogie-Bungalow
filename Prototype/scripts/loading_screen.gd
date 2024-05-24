extends Node

@export var animator : AnimationPlayer
@export var progress_bar : ProgressBar
@export var label_connection : Label
@export var skip_sync : bool = false

var disabled : bool = false
var block_finish : bool = false

signal send_sync_request_to_hardware()

func _ready():
	if skip_sync:
		queue_free()
		return
	
	EventBus.sync_package_send.connect( func(): progress_bar.value+= 1 )
	EventBus.sync_successful.connect( finish_connecting )
	EventBus.sync_restart.connect( restart_connecting )

func _input(event):
	if disabled:
		return
	
	if event is InputEventMouseButton and event.button_index==1:
		disabled = true
		animator.play("fade_in")
		EventBus.sync_start.emit()
		send_sync_request_to_hardware.emit()
	

func finish_connecting():
	if block_finish:
		return
		
	block_finish = true
	progress_bar.value+= 2
	animator.play("fade_out")
	await animator.animation_finished
	
	queue_free()

func restart_connecting():
	if block_finish:
		return
		
	progress_bar.value = 0
	label_connection.text = "Synchronisation fehlgeschlagen. Erneuter Versuch..."
	
