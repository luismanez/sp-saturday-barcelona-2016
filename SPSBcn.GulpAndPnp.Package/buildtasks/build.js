"use strict";

var gulp = require("gulp"),
    runSequence = require('run-sequence');

gulp.task('build', function (callback) {
    runSequence('clean', ['jshint', 'replace', 'minify-css', 'minify-js', 'copy-assets'], callback);
});

//gulp.task('build', function () {
//    console.log(global.settings);
//});

