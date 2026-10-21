const fs = require('fs');
const path = require('path');
const sharp = require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
const root = process.cwd();
const doc = JSON.parse(fs.readFileSync(path.join(root,'src/ui/layout/screens.json'),'utf8'));
const folder = path.join(root,'staging/validation/menu');
const out = path.join(folder,'overlays'); fs.mkdirSync(out,{recursive:true});
(async()=>{
 let count=0;
 const files=fs.readdirSync(folder).filter(n=>n.endsWith('.png'));
 for(const screen of doc.screens) for(const [style,p] of Object.entries(screen.styles)) {
  const svg=fs.readFileSync(path.join(root,'docs/design/wireframes',`${screen.id}_${style}.svg`));
  const diagram=await sharp(svg).extract({left:48,top:48,width:p.canvas[0],height:p.canvas[1]}).png().toBuffer();
  for(const name of files.filter(n=>n.startsWith(`${screen.id}_${style}_`))) {
   const file=path.join(folder,name); const meta=await sharp(file).metadata();
   const {data,info}=await sharp(diagram).resize(meta.width,meta.height).ensureAlpha().raw().toBuffer({resolveWithObject:true});
   for(let i=3;i<data.length;i+=4) data[i]=Math.round(data[i]*.4);
   await sharp(file).composite([{input:data,raw:{width:info.width,height:info.height,channels:4}}]).png().toFile(path.join(out,name));count++;
  }
 }
 for(const renderer of ['forward_plus','mobile']) for(const resolution of ['1280x720','1920x1080','2560x1440']) {
  const tiles=[];const tileW=400,tileH=250;
  for(let row=0;row<doc.screens.length;row++) for(let col=0;col<2;col++) {
   const style=['classic','enhanced'][col];const name=`${doc.screens[row].id}_${style}_${renderer}_${resolution}.png`;
   if(!files.includes(name))continue;
   tiles.push({input:await sharp(path.join(folder,name)).resize(tileW,225,{fit:'contain',background:'#eee'}).png().toBuffer(),left:col*tileW,top:row*tileH+25});
   const label=Buffer.from(`<svg width="400" height="25"><rect width="400" height="25" fill="#eee"/><text x="8" y="18" font-family="Segoe UI,sans-serif" font-size="13">${doc.screens[row].id} / ${style}</text></svg>`);
   tiles.push({input:label,left:col*tileW,top:row*tileH});
  }
  if(tiles.length) await sharp({create:{width:800,height:doc.screens.length*tileH,channels:3,background:'#eee'}}).composite(tiles).png().toFile(path.join(folder,`contact-sheet-${renderer}-${resolution}.png`));
 }
 fs.writeFileSync(path.join(folder,'overlay-manifest.json'),JSON.stringify({count,opacity:.4,source:'exact generated SVG cropped to canvas; bundled Sharp '+sharp.versions.sharp},null,2));
 console.log('Exact SVG overlays:',count,'Sharp:',sharp.versions.sharp);
})().catch(e=>{console.error(e);process.exit(1)});
