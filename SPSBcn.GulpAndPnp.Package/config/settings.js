var _ = require('lodash'),
    args = require('yargs').argv;

var defaultSettings = require('./default');

settings = {};

if (global.env.toLowerCase() === 'default') {
    //only need this for dev process when using spsave plugin
    var secrets = require('./secrets');
    settings.secrets = secrets.secrets;
}

var environmentSettings = require('./' + global.env);

settings.paths = _.merge({}, defaultSettings.paths, environmentSettings.paths);
settings.replacements = _.merge([], defaultSettings.replacements, environmentSettings.replacements);

//calculated settings
settings.paths.deployAssetsFolder = settings.paths.deployRootFolder + "/src";
settings.paths.deployStyleLibraryFolder = settings.paths.deployAssetsFolder + "/Style Library/spsbcn";
settings.replacements.push({ match: 'TimeStamp', replacement: new Date().getTime() });

//console.log(settings);

module.exports = settings;