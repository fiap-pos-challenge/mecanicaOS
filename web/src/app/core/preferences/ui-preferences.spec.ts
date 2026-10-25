import { TestBed } from '@angular/core/testing';
import { MemoryStorage } from '../../../testing/layout';
import { PREFERENCE_STORAGE, PREFERENCE_STORAGE_KEYS, UiPreferences } from './ui-preferences';

function setup(storage: Storage | null): UiPreferences {
  TestBed.configureTestingModule({
    providers: [{ provide: PREFERENCE_STORAGE, useValue: storage }],
  });
  return TestBed.inject(UiPreferences);
}

describe('UiPreferences', () => {
  it('returns null when nothing was stored', () => {
    expect(setup(new MemoryStorage()).read('theme')).toBeNull();
  });

  it('stores only the known interface keys', () => {
    const storage = new MemoryStorage();
    const preferences = setup(storage);

    preferences.write('sidebar', 'collapsed');
    preferences.write('theme', 'dark');

    expect(preferences.read('sidebar')).toBe('collapsed');
    expect(preferences.read('theme')).toBe('dark');
    expect(storage.keys().sort()).toEqual(Object.values(PREFERENCE_STORAGE_KEYS).sort());
  });

  it('ignores values that are not allowed for the key', () => {
    const storage = new MemoryStorage();
    storage.setItem(PREFERENCE_STORAGE_KEYS.theme, 'neon');

    expect(setup(storage).read('theme')).toBeNull();
  });

  it('keeps working when the storage throws', () => {
    const storage = new MemoryStorage();
    vi.spyOn(storage, 'getItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    vi.spyOn(storage, 'setItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    const preferences = setup(storage);

    expect(() => preferences.write('theme', 'light')).not.toThrow();
    expect(preferences.read('theme')).toBeNull();
  });

  it('works without storage', () => {
    const preferences = setup(null);

    preferences.write('sidebar', 'expanded');

    expect(preferences.read('sidebar')).toBeNull();
  });

  it('uses the browser storage by default', () => {
    expect(TestBed.inject(PREFERENCE_STORAGE)).toBe(globalThis.localStorage);
  });
});
