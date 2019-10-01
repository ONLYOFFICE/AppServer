import React from 'react';
import { storiesOf } from '@storybook/react';
import { action } from '@storybook/addon-actions';
import { BooleanValue } from 'react-values'
import { withKnobs, boolean } from '@storybook/addon-knobs/react';
import withReadme from 'storybook-readme/with-readme';
import Readme from './README.md';
import Section from '../../../.storybook/decorators/section';
import SidePanel from '.';
import Button from '../button';


storiesOf('Components|SidePanel', module)
  .addDecorator(withKnobs)
  .addDecorator(withReadme(Readme))
  .add('base', () => (
    <Section>
      <BooleanValue>
        {({ value, toggle }) => (
          <div>
            <Button
              label="Show"
              primary={true}
              size="medium"
              onClick={(e) => {
                action('onShow')(e);
                toggle(true);
              }}
            />
            <SidePanel
              visible={value}
              scale={boolean('scale', false)}
              headerContent="Change password"
              bodyContent={
                <div>Send the password change instruction to the <a href="mailto:asc@story.book">asc@story.book</a> email address</div>
              }
              footerContent={[
                <Button
                  key="SendBtn"
                  label="Send"
                  primary={true}
                  size="medium"
                  onClick={(e) => {
                    action('onOk')(e);
                    toggle(false);
                  }}
                />
              ]}
              onClose={e => {
                action('onClose')(e);
                toggle(false);
              }}
            />
          </div>
        )}
      </BooleanValue>  
    </Section>
  ));