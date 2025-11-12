r:10;
sticks: range(0,10) map (i)=>{
teta:2*i*math.pi/10;
x:r*math.cos(teta);
y:r*math.sin(teta);
eval { type: 'line'; from: [0,0]; to: [x,y]; };
};
ri: { type: 'circle'; center: [0,0]; radius: r*2/3; };
eval [sticks, ri];
