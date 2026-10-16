import { TestBed } from '@angular/core/testing';
import { IDBFactory as FakeIndexedDb } from 'fake-indexeddb';
import { OFFLINE_INDEXED_DB, OfflineQueueStorage } from './offline-queue.storage';

function setup(factory: IDBFactory | null = new FakeIndexedDb()) {
  TestBed.configureTestingModule({
    providers: [{ provide: OFFLINE_INDEXED_DB, useValue: factory }],
  });
  return TestBed.inject(OfflineQueueStorage);
}

describe('OfflineQueueStorage', () => {
  it('keeps the queue of the same user across instances', async () => {
    const factory = new FakeIndexedDb();
    await setup(factory).save('mechanic', 'user-1', [{ id: 'a' }, { id: 'b' }]);
    TestBed.resetTestingModule();

    expect(await setup(factory).load('mechanic', 'user-1')).toEqual([{ id: 'a' }, { id: 'b' }]);
  });

  it('discards the queue left by another user', async () => {
    const storage = setup();
    await storage.save('mechanic', 'user-1', [{ id: 'a' }]);

    expect(await storage.load('mechanic', 'user-2')).toEqual([]);
    expect(await storage.load('mechanic', 'user-1')).toEqual([]);
  });

  it('removes the record when the queue is empty', async () => {
    const storage = setup();
    await storage.save('mechanic', 'user-1', [{ id: 'a' }]);
    await storage.save('mechanic', 'user-1', []);

    expect(await storage.load('mechanic', 'user-1')).toEqual([]);
  });

  it('deletes the whole database on clear and opens it again afterwards', async () => {
    const storage = setup();
    await storage.save('mechanic', 'user-1', [{ id: 'a' }]);
    await storage.clear();

    expect(await storage.load('mechanic', 'user-1')).toEqual([]);
  });

  it('works as a no-op when the browser has no IndexedDB', async () => {
    const storage = setup(null);
    await storage.save('mechanic', 'user-1', [{ id: 'a' }]);
    await storage.clear();

    expect(await storage.load('mechanic', 'user-1')).toEqual([]);
  });
});
