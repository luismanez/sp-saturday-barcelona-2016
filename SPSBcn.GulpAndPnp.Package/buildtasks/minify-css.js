"use strict";

var gulp = require("gulp"),
    concat = require('gulp-concat'),
    uglifycss = require('gulp-uglifycss'),
    sass = require('gulp-sass'),
    gulpif = require('gulp-if');

gulp.task('minify-css', function () {
    
    var uglifyStyles = global.env.toLowerCase() === 'prod';

    return gulp.src(global.settings.paths.styles)
      .pipe(sass())
      .pipe(concat(global.settings.paths.stylesMinName))
      .pipe(gulpif(uglifyStyles, uglifycss()))
      .pipe(gulp.dest(global.settings.paths.deployStyleLibraryFolder + '/css'));
});