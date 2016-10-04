"use strict";

var gulp = require("gulp"),
    jshint = require('gulp-jshint');


gulp.task('jshint', function () {
    return gulp.src(["./**/app/**/*.js", "!./**/external/"])
		.pipe(jshint())
		.pipe(jshint.reporter('default'));
});