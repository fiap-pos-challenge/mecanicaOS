import { TestBed } from '@angular/core/testing';
import { MemoryStorage } from '../../../testing/layout';
import { PREFERENCE_STORAGE, PREFERENCE_STORAGE_KEYS } from '../preferences/ui-preferences';
import { ThemeService } from './theme.service';

function setup(storage = new MemoryStorage()): { theme: ThemeService; storage: MemoryStorage } {
  TestBed.configureTestingModule({
    providers: [{ provide: PREFERENCE_STORAGE, useValue: storage }],
  });
  return { theme: TestBed.inject(ThemeService), storage };
}

describe('ThemeService', () => {
  afterEach(() => document.documentElement.removeAttribute('data-theme'));

  it('follows the system theme by default', () => {
    const { theme } = setup();
    TestBed.tick();

    expect(theme.mode()).toBe('system');
    expect(document.documentElement.hasAttribute('data-theme')).toBe(false);
  });

  it('applies and remembers an explicit theme', () => {
    const { theme, storage } = setup();

    theme.set('dark');
    TestBed.tick();

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(storage.getItem(PREFERENCE_STORAGE_KEYS.theme)).toBe('dark');
  });

  it('cycles light, dark and system', () => {
    const storage = new MemoryStorage();
    storage.setItem(PREFERENCE_STORAGE_KEYS.theme, 'light');
    const { theme } = setup(storage);

    expect(theme.mode()).toBe('light');
    expect(theme.next()).toBe('dark');
    theme.cycle();
    expect(theme.mode()).toBe('dark');
    theme.cycle();
    expect(theme.mode()).toBe('system');
    TestBed.tick();
    expect(document.documentElement.hasAttribute('data-theme')).toBe(false);
  });
});
