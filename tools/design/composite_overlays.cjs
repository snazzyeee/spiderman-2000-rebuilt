const fs = require('fs');
const path = require('path');
const sharp = require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
const root = process.cwd();
const doc = JSON.parse(fs.readFileSync(path.join(root,'src/ui/layout/screens.json'),'utf8'));
const folder = path.join(root,'staging/validation/menu');
const out = path.join(folder,'overlays'); fs.mkdirSync(out,{recursive:true});
(async()=>{
 const polish=[];
 for(let row=0;row<2;row++) for(let col=0;col<2;col++) {
  const style=['classic','enhanced'][row];
  const name=`main_menu_${style}_forward_plus_1920x1080.png`;
  const file=path.join(folder,col===0?'polish_before':'',name);
  if(fs.existsSync(file)) {
   polish.push({input:await sharp(file).resize(960,540).png().toBuffer(),left:col*960,top:row*574+34});
   const label=Buffer.from(`<svg width="960" height="34"><rect width="960" height="34" fill="#eee"/><text x="12" y="23" font-family="Segoe UI,sans-serif" font-size="18">${style} / ${col===0?'before':'after (remaining gates in HANDOFF.md)'}</text></svg>`);
   polish.push({input:label,left:col*960,top:row*574});
  }
 }
 if(polish.length===8) await sharp({create:{width:1920,height:1148,channels:3,background:'#eee'}}).composite(polish).png().toFile(path.join(folder,'polish_before_after.png'));
 let count=0;
 const files=fs.readdirSync(folder).filter(n=>n.endsWith('.png'));
 for(const screen of doc.screens) for(const [style,p] of Object.entries(screen.styles)) {
  const svg=fs.readFileSync(path.join(root,'docs/design/wireframes',`${screen.id}_${style}.svg`));
  const diagram=await sharp(svg).extract({left:48,top:48,width:p.canvas[0],height:p.canvas[1]}).png().toBuffer();
  for(const name of files.filter(n=>n.startsWith(`${screen.id}_${style}_`))) {
   const file=path.join(folder,name); const meta=await sharp(file).metadata();
   const expected=name.match(/_(\d+)x(\d+)\.png$/);
   if(!expected || meta.width!==Number(expected[1]) || meta.height!==Number(expected[2])) throw new Error('Capture dimensions differ from requested resolution: '+name);
   const scale=Math.min(meta.width/p.canvas[0],meta.height/p.canvas[1]);
   const drawWidth=Math.round(p.canvas[0]*scale),drawHeight=Math.round(p.canvas[1]*scale);
   const left=Math.round((meta.width-drawWidth)/2),top=Math.round((meta.height-drawHeight)/2);
   const {data,info}=await sharp(diagram).resize(drawWidth,drawHeight).ensureAlpha().raw().toBuffer({resolveWithObject:true});
   for(let i=3;i<data.length;i+=4) data[i]=Math.round(data[i]*.4);
   await sharp(file).composite([{input:data,raw:{width:info.width,height:info.height,channels:4},left,top}]).png().toFile(path.join(out,name));count++;
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
 fs.writeFileSync(path.join(folder,'overlay-manifest.json'),JSON.stringify({count,opacity:.4,validatedCaptureDimensions:count,source:'exact generated SVG cropped to canvas; bundled Sharp '+sharp.versions.sharp},null,2));
 console.log('Exact SVG overlays:',count,'Sharp:',sharp.versions.sharp);
})().catch(e=>{console.error(e);process.exit(1)});
