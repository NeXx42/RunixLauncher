OUTPUT_DIR = ./Build/Output/

publish:
	rm -rf ${OUTPUT_DIR}/*
	
	# gtk overlay
	cd GameLibrary_GTKOverlay && cargo build --release
			
	# app
	dotnet publish RunixLauncher/RunixLauncher.csproj \
		-c Release \
		-r linux-x64 \
		--self-contained true \
		/p:PublishSingleFile=false \
		/p:IncludeAllContentForSelfExtract=true \
		-o ${OUTPUT_DIR}/RunixLauncher
		
	tar -czvf ${OUTPUT_DIR}/RunixLauncher.tar.gz -C ${OUTPUT_DIR} RunixLauncher

publish-appimage:
	rm -rf ${OUTPUT_DIR}/*
	
	# gtk overlay
	cd GameLibrary_GTKOverlay && cargo build --release

	# app
	dotnet publish RunixLauncher/RunixLauncher.csproj \
		-c Release \
		-r linux-x64 \
		--self-contained true \
		/p:PublishSingleFile=false \
		-o ${OUTPUT_DIR}/RunixLauncher
		
	mkdir -p ${OUTPUT_DIR}/RunixLauncher.AppDir/usr/bin
	mkdir -p ${OUTPUT_DIR}/RunixLauncher.AppDir/usr/share/icons/hicolor/256x256/apps/
	
	cp -r ${OUTPUT_DIR}/RunixLauncher/* ${OUTPUT_DIR}/RunixLauncher.AppDir/usr/bin
	
	cp ./Build/AppImageData/* ${OUTPUT_DIR}/RunixLauncher.AppDir
	cp -r ./Build/AppImageData/*.png ${OUTPUT_DIR}/RunixLauncher.AppDir/usr/share/icons/hicolor/256x256/apps/
	
	appimagetool ${OUTPUT_DIR}/RunixLauncher.AppDir ${OUTPUT_DIR}/RunixLauncher.appimage
	chmod +x ${OUTPUT_DIR}/RunixLauncher.appimage

overlay-debug:
	cd GameLibrary_GTKOverlay && cargo build
	
overlay:
	cd GameLibrary_GTKOverlay && cargo build --release