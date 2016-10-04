"use strict";

var gulp = require("gulp"),
    spsave = require('gulp-spsave');


var quickDeploy = function (srcData, destinationFolder, flatten, checkin, checkinType) {
    return gulp.src(srcData)
		.pipe(spsave({
		    username: global.settings.secrets.sharepoint.user,
		    password: global.settings.secrets.sharepoint.password,
		    siteUrl: global.settings.secrets.sharepoint.sitecollection,
		    folder: destinationFolder,
		    flatten: flatten,
		    checkin: checkin,
		    checkinType: checkinType
		}));
};

gulp.task('spcopy-js', ['minify-js'], function () {
    return quickDeploy(
        [
            global.settings.paths.deployStyleLibraryFolder + "/js/*.js",
            "!" + global.settings.paths.deployStyleLibraryFolder + "/js/vendor/**"
        ],
        "Style Library/spsbcn/js", false, true, 1);
});

gulp.task('spcopy-css', ['minify-css'], function () {

    return quickDeploy(
       [
           global.settings.paths.deployStyleLibraryFolder + "/css/*.css"
       ],
       "Style Library/spsbcn/css", false, true, 1);
});

gulp.task('spcopy-html', ['quick-build'], function () {

    return quickDeploy(
       [
           global.settings.paths.deployStyleLibraryFolder + "/html/*.html"
       ],
       "Style Library/spsbcn/html", false, true, 1);
});