import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { SessionStateRegistry } from '../auth/session-state.registry';
import { bindDraft, DraftStore } from './draft.store';

@Component({ selector: 'app-draft-host', template: '' })
class DraftHost {
  readonly form = new FormGroup({ name: new FormControl('', { nonNullable: true }) });
  readonly discard = bindDraft(this.form, 'customer-new');
}

describe('DraftStore', () => {
  it('keeps drafts in memory only for the owner of the session', () => {
    const drafts = TestBed.inject(DraftStore);
    drafts.save('form', { name: 'ignored' });
    expect(drafts.restore('form')).toBeNull();

    expect(drafts.claim('u1')).toBe('first');
    drafts.save('form', { name: 'Ana' });
    expect(drafts.claim('u1')).toBe('same');
    expect(drafts.restore('form')).toEqual({ name: 'Ana' });

    expect(drafts.claim('u2')).toBe('changed');
    expect(drafts.restore('form')).toBeNull();
  });

  it('returns copies, discards one draft and clears everything on logout', () => {
    const drafts = TestBed.inject(DraftStore);
    drafts.claim('u1');
    const value = { items: [1] };
    drafts.save('a', value);
    drafts.save('b', 1);
    value.items.push(2);

    expect(drafts.restore('a')).toEqual({ items: [1] });
    drafts.discard('a');
    expect(drafts.restore('a')).toBeNull();
    TestBed.inject(SessionStateRegistry).resetAll();
    expect(drafts.restore('b')).toBeNull();
  });

  it('restores a bound form after the component comes back', () => {
    TestBed.inject(DraftStore).claim('u1');
    const first = TestBed.createComponent(DraftHost);
    first.componentInstance.form.controls.name.setValue('Rascunho');
    first.destroy();

    const second = TestBed.createComponent(DraftHost);

    expect(second.componentInstance.form.controls.name.value).toBe('Rascunho');
    expect(second.componentInstance.form.dirty).toBe(true);
    second.componentInstance.discard();
    expect(TestBed.inject(DraftStore).restore('customer-new')).toBeNull();
  });
});
