/**
 * config/dogs.ts — dog coat palettes, ported from the prototype's DOGS object.
 * The "wet" variant is DERIVED (each channel shaded -34) so the slick/swim coat
 * stays in sync if a base colour changes — no hand-maintained second palette.
 */

import { shade } from '../core/math.js';

export type DogId = 'cheddar' | 'cocoa';

export interface Palette {
  name: string;
  body: [string, string];
  ear: [string, string];
  chest: string;
  nose: string;
  eye: string;
  outline: string;
}

const BASE: Record<DogId, Palette> = {
  cheddar: {
    name: 'CHEDDAR',
    body: ['#f6dcb2', '#e3ab63'],
    ear: ['#e9bd84', '#cf924c'],
    chest: '#fbf0da',
    nose: '#7a5536',
    eye: '#5d4226',
    outline: 'rgba(120,80,40,.35)',
  },
  cocoa: {
    name: 'COCOA',
    body: ['#7d4e2d', '#4d2d17'],
    ear: ['#5e3a20', '#3c2410'],
    chest: '#8a5b38',
    nose: '#241307',
    eye: '#33200f',
    outline: 'rgba(40,22,10,.4)',
  },
};

function wetOf(o: Palette): Palette {
  return {
    name: o.name,
    body: [shade(o.body[0], -34), shade(o.body[1], -34)],
    ear: [shade(o.ear[0], -34), shade(o.ear[1], -34)],
    chest: shade(o.chest, -34),
    nose: o.nose,
    eye: o.eye,
    outline: o.outline,
  };
}

export interface DogPalette {
  dry: Palette;
  wet: Palette;
}

export const DOGS: Record<DogId, DogPalette> = {
  cheddar: { dry: BASE.cheddar, wet: wetOf(BASE.cheddar) },
  cocoa: { dry: BASE.cocoa, wet: wetOf(BASE.cocoa) },
};
