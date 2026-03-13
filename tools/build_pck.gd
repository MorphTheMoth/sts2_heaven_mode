extends SceneTree

func find_mod_image_ctex(imported_dir: String) -> String:
	var dir := DirAccess.open(imported_dir)
	if dir == null:
		return ""
	for file in dir.get_files():
		if file.begins_with("mod_image.png-") and file.ends_with(".ctex"):
			return file
	return ""

func add_mod_image_import_chain(packer: PCKPacker, project_dir: String) -> int:
	var imported_dir := project_dir.path_join(".godot/imported")
	var ctex_name := find_mod_image_ctex(imported_dir)
	if ctex_name.is_empty():
		push_error("no imported ctex found for mod image")
		return ERR_FILE_NOT_FOUND
	var ctex_source := imported_dir.path_join(ctex_name)
	var ctex_target := "res://.godot/imported/%s" % ctex_name
	var add_ctex_ok := packer.add_file(ctex_target, ctex_source)
	if add_ctex_ok != OK:
		return add_ctex_ok
	var md5_name := "%s.md5" % ctex_name
	var md5_source := imported_dir.path_join(md5_name)
	if FileAccess.file_exists(md5_source):
		var add_md5_ok := packer.add_file("res://.godot/imported/%s" % md5_name, md5_source)
		if add_md5_ok != OK:
			return add_md5_ok
	return OK

func add_external_dir(packer: PCKPacker, source_dir: String, target_dir: String) -> int:
	var dir := DirAccess.open(source_dir)
	if dir == null:
		return OK
	var files := dir.get_files()
	for file in files:
		var source_file := source_dir.path_join(file)
		var target_file := target_dir.path_join(file)
		var add_file_ok := packer.add_file(target_file, source_file)
		if add_file_ok != OK:
			return add_file_ok
	var directories := dir.get_directories()
	for directory in directories:
		var recurse_ok := add_external_dir(packer, source_dir.path_join(directory), target_dir.path_join(directory))
		if recurse_ok != OK:
			return recurse_ok
	return OK

func _initialize():
	var output_dir := "res://build"
	var output_file := "res://build/HeavenMode.pck"
	var manifest_path := "res://mod_manifest.json"
	var project_dir := ProjectSettings.globalize_path("res://")
	var external_asset_dir := project_dir.path_join("HeavenMode")
	DirAccess.make_dir_recursive_absolute(output_dir)
	var packer := PCKPacker.new()
	var ok := packer.pck_start(output_file)
	if ok != OK:
		push_error("pck_start failed: %s" % ok)
		quit(1)
	var add_manifest_ok := packer.add_file(manifest_path, manifest_path)
	if add_manifest_ok != OK:
		push_error("add_file failed: %s %s" % [manifest_path, add_manifest_ok])
		quit(1)
	var add_external_ok := add_external_dir(packer, external_asset_dir, "res://HeavenMode")
	if add_external_ok != OK:
		push_error("add_external_dir failed: %s" % add_external_ok)
		quit(1)
	if FileAccess.file_exists(external_asset_dir.path_join("mod_image.png")):
		var add_import_chain_ok := add_mod_image_import_chain(packer, project_dir)
		if add_import_chain_ok != OK:
			push_error("add_mod_image_import_chain failed: %s" % add_import_chain_ok)
			quit(1)
	var flush_ok := packer.flush()
	if flush_ok != OK:
		push_error("flush failed: %s" % flush_ok)
		quit(1)
	print("PCK built: %s" % output_file)
	quit(0)
