"use strict";

var concat = require('gulp-concat'),
    uglify = require('gulp-uglify'),
    sourcemaps = require('gulp-sourcemaps'),
    order = require("gulp-order"),
    gulp = require("gulp"),
    print = require('gulp-print'),
    gulpif = require('gulp-if');

gulp.task('minify-js', function () {

    var notProdEnvironment = global.env.toLowerCase() !== "prod";

    return gulp.src(global.settings.paths.scripts)
      .pipe(order(global.settings.paths.scriptsOrder))
      //.pipe(print())
      .pipe(gulpif(notProdEnvironment, sourcemaps.init()))      
      .pipe(concat(global.settings.paths.scriptsMinName))
      .pipe(gulpif(!notProdEnvironment, uglify()))
      .pipe(gulpif(notProdEnvironment, sourcemaps.write()))
      .pipe(gulp.dest(global.settings.paths.deployStyleLibraryFolder + '/js'));
});