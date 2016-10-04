"use strict";

var gulp = require("gulp"),
    args = require('yargs').argv;

global.env = args.env || 'default';
global.settings = require('./config/settings.js');

require("./buildtasks/replace.js");
require("./buildtasks/clean.js");
require("./buildtasks/minify-js.js");
require("./buildtasks/minify-css.js");
require("./buildtasks/jshint.js");
require("./buildtasks/copy-assets.js");
require("./buildtasks/build.js");
require("./buildtasks/quick-build.js");
require("./buildtasks/spcopy.js");
require("./buildtasks/default.js");
require("./buildtasks/sync.js");