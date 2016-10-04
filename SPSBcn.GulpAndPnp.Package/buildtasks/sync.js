"use strict";

var gulp = require("gulp"),
    util = require('gulp-util');

gulp.task("sync", function () {        

    var watcherJs = gulp.watch([        
        global.settings.paths.scripts
    ], ['spcopy-js']);

    var watcherCss = gulp.watch([
        global.settings.paths.styles
    ], ['spcopy-css']);    

});