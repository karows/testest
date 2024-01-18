const crypto = require('crypto');
const fs = require('fs');
const glob = require('glob');

const jsonFilesDir = 'dist/browser/assets/langs/'; // Adjust the path to your JSON files
const outputDir = 'dist/browser/assets/langs'; // Directory to store minified files

function generateChecksum(str, algorithm, encoding) {
    return crypto
        .createHash(algorithm || 'md5')
        .update(str, 'utf8')
        .digest(encoding || 'hex');
}

const result = {};

glob.sync(`${jsonFilesDir}**/*.json`).forEach(path => {
    const [_, lang] = path.split('dist\\browser\\assets\\langs\\');
    const content = fs.readFileSync(path, { encoding: 'utf-8' });
    result[lang.replace('.json', '')] = generateChecksum(content);
});

fs.writeFileSync('./i18n-cache-busting.json', JSON.stringify(result));
fs.writeFileSync(`dist/browser/i18n-cache-busting.json`, JSON.stringify(result));
