HP={'Bat':160,'BatSwing':160,'Zombi':350,'Ghost':180,'Baku':250,'BakuRegen':500}
IMMUNE={'Baku','BakuRegen'}
BAR={'Zombi':500}
def wave(name,sd,hpr,br,groups):
    t=sd; cnt={}; last=sd
    for i,(nxt,entries) in enumerate(groups):
        gs=t
        for (e,n,d,iv) in entries:
            end=gs+d+iv*(n-1)
            last=max(last,end)
            cnt[e]=cnt.get(e,0)+n
        if i<len(groups)-1: t=gs+nxt
    tot=sum(cnt.values())
    dps=sum(HP[e]*n for e,n in cnt.items() if e not in IMMUNE)*hpr
    imm=sum(HP[e]*n for e,n in cnt.items() if e in IMMUNE)*hpr
    bar=sum(BAR.get(e,0)*n for e,n in cnt.items())*br
    print(f"{name}: 体数{tot} 最終湧き{last:.1f}s 実HP{dps:,.0f} 無敵HP{imm:,.0f} バリア{bar:,.0f}  {cnt}")

wave("Early_A",0.5,1.15,1.2,[
 (10,[('Bat',8,0,0.8),('BatSwing',3,2,2.0)]),
 (11,[('Bat',9,0,0.7),('BatSwing',4,1,1.8)]),
 (11,[('BatSwing',6,0,1.2),('Bat',9,1,0.7)]),
 (0,[('Bat',10,0,0.6),('BatSwing',5,2,1.2),('Baku',1,5,0)]),
])
wave("Early_B",0.5,1.15,1.2,[
 (11,[('Zombi',3,0,1.8),('Bat',8,1,0.8)]),
 (12,[('BakuRegen',1,0,0),('Zombi',3,1,1.6),('Bat',8,2,0.7)]),
 (12,[('Bat',10,0,0.6),('Zombi',2,2,2.0)]),
 (0,[('BakuRegen',2,0,3),('Zombi',2,1,1.5),('Bat',8,2,0.6)]),
])
wave("Early_C",0.5,1.15,1.2,[
 (10,[('Ghost',5,0,1.4),('Bat',6,1,0.9)]),
 (11,[('Ghost',6,0,1.2),('BatSwing',4,2,1.5)]),
 (11,[('Bat',10,0,0.6),('Ghost',5,2,1.4)]),
 (0,[('Ghost',6,0,1.1),('Bat',8,1,0.6),('Baku',2,5,3)]),
])
wave("Middle_A",0.5,1.4,1.5,[
 (9,[('Bat',8,0,0.7),('Zombi',2,1,2.0)]),
 (10,[('Ghost',5,0,1.2),('BatSwing',4,1,1.5)]),
 (10,[('Bat',9,0,0.6),('Zombi',2,2,1.8),('Baku',2,5,3)]),
 (10,[('BatSwing',5,0,1.2),('Ghost',5,1,1.1)]),
 (0,[('Bat',9,0,0.55),('Zombi',2,1,1.6),('Ghost',4,3,1.2)]),
])
wave("Middle_B",0.5,1.4,1.5,[
 (9,[('Bat',10,0,0.6),('Baku',2,3,3)]),
 (10,[('Zombi',4,0,1.5),('Bat',8,1,0.7),('BakuRegen',1,6,0)]),
 (10,[('BatSwing',6,0,1.1),('Baku',3,2,2.5)]),
 (10,[('Bat',10,0,0.55),('Zombi',3,1,1.6),('BakuRegen',2,5,3)]),
 (0,[('Ghost',6,0,1.1),('Bat',8,1,0.6),('Baku',2,4,3)]),
])
wave("Late_A",0.5,1.7,1.8,[
 (8,[('Bat',9,0,0.6),('Zombi',2,1,2.0)]),
 (9,[('Ghost',5,0,1.1),('BatSwing',4,1,1.4)]),
 (9,[('Bat',9,0,0.55),('Zombi',2,1,1.8),('Baku',2,5,2.5)]),
 (9,[('BatSwing',5,0,1.1),('Ghost',5,1,1.1),('BakuRegen',2,5,3)]),
 (10,[('Bat',10,0,0.5),('Zombi',2,1,1.6),('Ghost',4,3,1.2)]),
 (0,[('Bat',9,0,0.45),('BatSwing',5,1,1.0),('Zombi',2,2,1.5),('Baku',2,4,2.5)]),
])
