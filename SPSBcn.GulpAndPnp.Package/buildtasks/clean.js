"use strict";

var gulp = require("gulp"),
    clean = require('gulp-clean');

gulp.task('clean', function () {
    var directories = [];
    directories.push("dist/*");

    return gulp.src(directories, { read: false })
        .pipe(clean());
});