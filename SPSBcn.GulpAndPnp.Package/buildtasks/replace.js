"use strict";

var gulp = require('gulp'),
    replace = require('gulp-replace-task'),
    rename = require('gulp-rename'),
    include = require('gulp-include'),
    gulpif = require('gulp-if');

gulp.task('replace', function () {
    
    var addDemoData = global.env.toLowerCase() !== 'prod';

    return gulp.src(global.settings.paths.replacementsSrc)
        .pipe(gulpif(addDemoData, include()))
        .pipe(replace({
            patterns: global.settings.replacements
        }))
        .pipe(rename(function (path) {
            if (path.extname === ".xml") {
                path.dirname = "/pnp";
            } else {
                path.dirname = "../" + path.dirname;
            }
        }))
        .pipe(gulp.dest(global.settings.paths.deployAssetsFolder));

});