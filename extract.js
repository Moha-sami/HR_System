const fs = require('fs');
const path = require('path');
const dir = 'tests/Buy2.Domain.Tests/StrykerOutput';
const latest = fs.readdirSync(dir).filter(d => fs.statSync(path.join(dir, d)).isDirectory()).sort((a,b) => fs.statSync(path.join(dir, b)).ctimeMs - fs.statSync(path.join(dir, a)).ctimeMs)[0];
const html = fs.readFileSync(path.join(dir, latest, 'reports', 'mutation-report.html'), 'utf8');

const startStr = 'app.report = ';
const start = html.indexOf(startStr);
if (start !== -1) {
    let jsonStr = html.substring(start + startStr.length);
    const endStr = '"thresholds":{"high":80,"low":60}}';
    const end = jsonStr.indexOf(endStr);
    jsonStr = jsonStr.substring(0, end + endStr.length);
    try {
        const data = JSON.parse(jsonStr);
        const files = Object.keys(data.files);
        const myFile = files.find(f => f.includes('ReassignAndDeleteJobCommand.cs'));
        const mutants = data.files[myFile].mutants.filter(m => m.status === 'Survived');
        mutants.forEach(m => console.log(m.mutatorName, m.location.start.line, m.replacement));
    } catch(e) {
        console.log(e.message);
    }
}
