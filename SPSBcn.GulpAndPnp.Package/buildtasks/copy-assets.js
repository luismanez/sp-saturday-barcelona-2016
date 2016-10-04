"use strict";

var gulp = require("gulp");

gulp.task('copy-assets', function () {
    return gulp.src([
        "./src/**",
        "!./**/js/{app,app/**}",
        "!./**/scss/**",        
        "!./**/masterpage/**/*.aspx",
        "!./**/masterpage/**/*.master"])
        .pipe(gulp.dest(global.settings.paths.deployAssetsFolder));
});