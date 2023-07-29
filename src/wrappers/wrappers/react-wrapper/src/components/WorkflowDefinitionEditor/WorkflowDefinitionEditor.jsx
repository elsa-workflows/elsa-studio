import React from 'react';
import PropTypes from 'prop-types';
import {v4 as uuid} from 'uuid';


/**
 * Primary UI component for user interaction
 */
export const WorkflowDefinitionEditor = ({ definitionId, ...props }) => {
  const id = uuid();
  return React.createElement('elsa-studio-workflow-definition-editor', { ...props, id: id, "definition-id": definitionId });
};

WorkflowDefinitionEditor.propTypes = {
  /**
   * Is this the principal call to action on the page?
   */
  definitionId: PropTypes.string,
};

WorkflowDefinitionEditor.defaultProps = {
  definitionId: null,
};
